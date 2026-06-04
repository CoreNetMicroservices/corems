import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Spinner } from 'react-bootstrap';
import { Camera } from 'react-bootstrap-icons';
import { uploadDocumentMultipart } from '@/document/store/DocumentState';
import { getDocumentDownloadUrl } from '@/document/store/DocumentState';
import { useMessageState } from '@/common/utils/api/ApiResponseHandler';

interface ProfileImageUploadProps {
  currentImageUrl?: string;
  onImageUpdate: (imageUrl: string) => Promise<void>;
  size?: number;
  disabled?: boolean;
  className?: string;
  ownerUserId?: string;
}

const ProfileImageUpload: React.FC<ProfileImageUploadProps> = ({
  currentImageUrl,
  onImageUpdate,
  size = 96,
  disabled = false,
  className = '',
  ownerUserId
}) => {
  const { t } = useTranslation();
  const [isUploading, setIsUploading] = useState(false);
  const { handleResponse } = useMessageState();

  const handleImageUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'invalid.file', description: 'Please select an image file' }] },
        'Please select an image file',
        ''
      );
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'file.too.large', description: 'File size must be less than 5MB' }] },
        'File size must be less than 5MB',
        ''
      );
      return;
    }

    setIsUploading(true);

    try {
      const fileExtension = file.name.split('.').pop() || 'jpg';
      const profilePictureName = `profile-picture.${fileExtension}`;
      
      const renamedFile = new File([file], profilePictureName, { type: file.type });
      
      const uploadResult = await uploadDocumentMultipart(renamedFile, {
        ownerUserId: ownerUserId,
        visibility: 'PUBLIC',
        tags: ['profile-picture'],
        confirmReplace: true
      });

      if (uploadResult.result && uploadResult.response) {
        const imageUrl = uploadResult.response.viewUrl || getDocumentDownloadUrl(uploadResult.response.uuid);
        const cacheBustedUrl = `${imageUrl}?t=${Date.now()}`;
        
        await onImageUpdate(cacheBustedUrl);
        
        handleResponse(
          { result: true, response: null, errors: [] },
          '',
          'Profile image updated successfully!'
        );
      } else {
        handleResponse(
          uploadResult,
          'Failed to upload image',
          ''
        );
      }
    } catch {
      handleResponse(
        { result: false, response: null, errors: [{ reasonCode: 'upload.failed', description: 'Failed to upload image' }] },
        'Failed to upload image',
        ''
      );
    } finally {
      setIsUploading(false);
      event.target.value = '';
    }
  };

  const inputId = `profile-image-upload-${Math.random().toString(36).substring(2, 11)}`;

  return (
    <div className={`text-center position-relative ${className}`}>
      {currentImageUrl ? (
        <div className="position-relative d-inline-block">
          <img
            src={currentImageUrl}
            alt="Profile"
            style={{ 
              width: size, 
              height: size, 
              borderRadius: '50%', 
              objectFit: 'cover', 
              border: '2px solid var(--bs-primary)',
              opacity: disabled ? 0.6 : 1
            }}
          />
          {!disabled && (
            <label 
              htmlFor={inputId}
              className="position-absolute bottom-0 end-0 btn btn-primary btn-sm rounded-circle p-1"
              style={{ cursor: 'pointer', width: 32, height: 32 }}
              title={t('profile.changeImage', 'Change profile image')}
            >
              {isUploading ? (
                <Spinner animation="border" size="sm" />
              ) : (
                <Camera size={16} />
              )}
            </label>
          )}
        </div>
      ) : (
        <label 
          htmlFor={disabled ? undefined : inputId} 
          style={{ 
            cursor: disabled ? 'default' : 'pointer', 
            display: 'inline-block',
            opacity: disabled ? 0.6 : 1
          }}
        >
          <div style={{
            width: size,
            height: size,
            borderRadius: '50%',
            background: '#e9ecef',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: '2px dashed var(--bs-primary)',
            fontSize: Math.floor(size / 4),
            color: 'var(--bs-primary)'
          }}>
            {isUploading ? (
              <Spinner animation="border" variant="primary" />
            ) : (
              <Camera size={Math.floor(size / 4)} />
            )}
          </div>
        </label>
      )}
      {!disabled && (
        <input
          id={inputId}
          type="file"
          accept="image/*"
          style={{ display: 'none' }}
          onChange={handleImageUpload}
          disabled={isUploading}
        />
      )}
    </div>
  );
};

export default ProfileImageUpload;